import React, { useEffect, useRef, useState } from 'react';
import * as d3 from 'd3';
import { feature } from 'topojson-client';
import { geoPath, geoMercator } from 'd3-geo';
import usData from './assets/united_states.json';
import everythingExport from './assets/everything_export.json';
import averageCoordinates from './assets/everything_export.json';
import junction from './assets/I80-I880-junction.json';
import insterstateNorth from './assets/I29-North_smoothed.json';
import omahaNorth from './assets/I680-curve_smoothed.json';
import graph from './assets/graph.json';
import bestI29North from './assets/path-I29.json';
import bestOmahaNorth from './assets/path-I680.json';
import omahaNorthPredictedExits from './assets/I680-new-points.json';
import northPredictedExits from './assets/I29-new-points.json';
import bestPath from './assets/path.json';
import newPoints from './assets/new-points.json';


interface FeatureCollection {
  type: string;
  features: Array<any>;
}
const dataGathering = false;
//allows for easily switching between what data is being mapped
const predictedRoad: FeatureCollection = (dataGathering ? averageCoordinates : bestOmahaNorth) as FeatureCollection;
const predictedExits = omahaNorthPredictedExits as FeatureCollection;


const actualRoad: FeatureCollection = omahaNorth as FeatureCollection;
const actualExits = omahaNorth as FeatureCollection;

const USMap: React.FC = () => {
  //adjust based on screen size
  const updateDimensions = () => {
    const width = window.innerWidth - (window.innerWidth > document.documentElement.clientWidth ? 17 : 0);
    const height = window.innerHeight - (window.innerHeight > document.documentElement.clientHeight ? 17 : 0);
    return { width, height };
  };

  const [dimensions, setDimensions] = useState<{ width: number, height: number }>(updateDimensions());
  // State for selected features (storing indices)
  const [selectedFeatures, setSelectedFeatures] = useState<number[]>([]);
  // State for export file name
  const [exportFileName, setExportFileName] = useState<string>('exported_features.json');

  const svgRef = useRef<SVGSVGElement | null>(null);
  const gRef = useRef<SVGGElement | null>(null);
  // handle the resize event
  useEffect(() => {
    const handleResize = () => setDimensions(updateDimensions());
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  // Add zoom behavior
  useEffect(() => {
    const svg = d3.select(svgRef.current) as d3.Selection<SVGSVGElement, unknown, null, undefined>;
    const g = d3.select(gRef.current) as d3.Selection<SVGGElement, unknown, null, undefined>;

    const zoomBehavior = d3.zoom<SVGSVGElement, unknown>()
      .scaleExtent([1, 50])
      .on('zoom', (event) => {
        g.attr('transform', event.transform);
      });

    svg.call(zoomBehavior as any);
  }, []);

  // Add right-click drag selection behavior
  useEffect(() => {
    const svg = d3.select(svgRef.current);
    let isDragging = false;
    let startPoint: [number, number] | null = null;
    let selectionRect: d3.Selection<SVGRectElement, unknown, null, undefined> | null = null;

    // Prevent default context menu
    svg.on('contextmenu', (event: any) => {
      event.preventDefault();
    });
    //Allow gragging selection with right mouse button
    svg.on('mousedown', function (event: any) {
      if (event.button !== 2) return; // only act on right mouse button
      isDragging = true;
      startPoint = d3.pointer(event);

      // Create selection rectangle
      selectionRect = svg.append('rect')
        .attr('class', 'selection')
        .attr('x', startPoint[0])
        .attr('y', startPoint[1])
        .attr('width', 0)
        .attr('height', 0)
        .attr('fill', 'rgba(0, 0, 255, 0.1)')
        .attr('stroke', 'blue')
        .attr('stroke-width', 1);
    });

    svg.on('mousemove', function (event: any) {
      if (!isDragging || !startPoint || !selectionRect) return;
      const currentPoint = d3.pointer(event);
      const x = Math.min(startPoint[0], currentPoint[0]);
      const y = Math.min(startPoint[1], currentPoint[1]);
      const width = Math.abs(currentPoint[0] - startPoint[0]);
      const height = Math.abs(currentPoint[1] - startPoint[1]);
      selectionRect
        .attr('x', x)
        .attr('y', y)
        .attr('width', width)
        .attr('height', height);
    });

    svg.on('mouseup', function (event: any) {
      if (event.button !== 2) return;
      if (isDragging && startPoint && selectionRect) {
        const endPoint = d3.pointer(event);
        const x0 = Math.min(startPoint[0], endPoint[0]);
        const y0 = Math.min(startPoint[1], endPoint[1]);
        const x1 = Math.max(startPoint[0], endPoint[0]);
        const y1 = Math.max(startPoint[1], endPoint[1]);

        // Get current transform applied to group elements
        const transform = d3.zoomTransform(gRef.current as SVGGElement);
        // Identify features that fall inside the selection rectangle
        const selected = predictedRoad.features.reduce((acc: number[], feature, i) => {
          if (!feature.geometry || !feature.geometry.coordinates) return acc;
          const coords = projection(feature.geometry.coordinates as any) as [number, number];
          // Apply current zoom/pan transform to the computed projection
          const [cx, cy] = transform.apply(coords);
          if (cx >= x0 && cx <= x1 && cy >= y0 && cy <= y1) {
            acc.push(i);
          }
          return acc;
        }, [] as number[]);
        setSelectedFeatures(selected);

        // Clean up the selection rectangle
        selectionRect.remove();
        selectionRect = null;
      }
      isDragging = false;
      startPoint = null;
    });

    // Cleanup on unmount
    return () => {
      svg.on('mousedown', null);
      svg.on('mousemove', null);
      svg.on('mouseup', null);
      svg.on('contextmenu', null);
    };
  }, [dimensions]);
  
  //Set up the map projection
  const projection = geoMercator()
    .translate([dimensions.width / 2, dimensions.height / 2])
    .center([-98.5, 39.5])
    .scale(1000);

  const path = geoPath().projection(projection);

  // Handle export
  const handleExport = () => {
    const exportFeatures = selectedFeatures.map(i => predictedRoad.features[i]);
    const exportData: FeatureCollection = {
      type: "FeatureCollection",
      features: exportFeatures
    };
    const dataStr = JSON.stringify(exportData, null, 2);
    const blob = new Blob([dataStr], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = exportFileName;
    a.click();
    URL.revokeObjectURL(url);
    // Deselect points after export
    setSelectedFeatures([]);
  };

  //Render the page
  return (
    <div style={{ width: '100%', height: 'auto' }}>
      <svg ref={svgRef} width={dimensions.width} height={dimensions.height}>
        <g ref={gRef}>
          <path
            d={path(feature(usData as any, usData.objects.states as any)) || ''}
            stroke="#aaa"
            strokeWidth="0.5"
            fill="none"
          />
          {predictedExits.features.map((feature, i) => {
            if (!feature.geometry || !feature.geometry.coordinates) return null;
            const [x, y] = projection(feature.geometry.coordinates as any) as [number, number];
            const isSelected = selectedFeatures.includes(i);
            return (
              <circle
                key={i}
                cx={x}
                cy={y}
                r={0.1}
                stroke={isSelected ? "green" : "#ff0000"}
                strokeWidth="0.05"
                fill={isSelected ? "green" : "none"}
              />
            );
          })}
          {!dataGathering && (() => {
            predictedRoad.features = predictedRoad.features
            .sort((a, b) => {
              const aRef = Number(a.properties.ref);
              const bRef = Number(b.properties.ref);
              return aRef - bRef;
            });
            const roadSegments = predictedRoad.features
              .map((feature, i) => {
                if (!feature.geometry || !feature.geometry.coordinates) return null;
                const point = projection(feature.geometry.coordinates as any) as [number, number];
                return { point, index: i };
              })
              .filter((seg): seg is { point: [number, number]; index: number } => seg !== null);

            return roadSegments.slice(0, -1).map((seg, i) => {
              const nextSeg = roadSegments[i + 1];
              // Consider the segment selected if both endpoints are selected
              const isSelectedSegment =
                selectedFeatures.includes(seg.index) && selectedFeatures.includes(nextSeg.index);
              return (
                <line
                  key={`line-${seg.index}`}
                  x1={seg.point[0]}
                  y1={seg.point[1]}
                  x2={nextSeg.point[0]}
                  y2={nextSeg.point[1]}
                  stroke={isSelectedSegment ? "green" : "#ff0000"}
                  strokeWidth="0.1"
                />
              );
            });
          })()}
          {!dataGathering && (() => {
            actualRoad.features = actualRoad.features
            .sort((a, b) => {
              const aRef = Number(a.properties.ref);
              const bRef = Number(b.properties.ref);
              return aRef - bRef;
            });
            const roadSegments = actualRoad.features
              .map((feature, i) => {
                if (!feature.geometry || !feature.geometry.coordinates) return null;
                const point = projection(feature.geometry.coordinates as any) as [number, number];
                return { point, index: i };
              })
              .filter((seg): seg is { point: [number, number]; index: number } => seg !== null);

            return roadSegments.slice(0, -1).map((seg, i) => {
              const nextSeg = roadSegments[i + 1];
              // Consider the segment selected if both endpoints are selected
              return (
                <line
                  key={`line-${seg.index}`}
                  x1={seg.point[0]}
                  y1={seg.point[1]}
                  x2={nextSeg.point[0]}
                  y2={nextSeg.point[1]}
                  stroke={"green"}
                  strokeWidth="0.05"
                />
              );
            });
          })()}
          {actualExits.features.map((feature, i) => {
            if (!feature.geometry || !feature.geometry.coordinates) return null;
            const [x, y] = projection(feature.geometry.coordinates as any) as [number, number];
            const isSelected = selectedFeatures.includes(i);
            return (
              <circle
                key={i}
                cx={x}
                cy={y}
                r={0.1}
                stroke={"green"}
                strokeWidth="0.1"
                fill={"none"}
              />
            );
          })}
          {/* ...existing code... */}
        </g>
      </svg>
      <div style={{ marginTop: '1rem' }}>
        <strong>Selected Feature Indices:</strong> {selectedFeatures.map(p => predictedRoad.features[p].geometry.coordinates).join(', ')}
      </div>
      <div style={{ marginTop: '1rem' }}>
        <input
          type="text"
          value={exportFileName}
          onChange={(e) => setExportFileName(e.target.value)}
          placeholder="Enter filename (e.g., exported_features.json)"
        />
        <button onClick={handleExport} style={{ marginLeft: '1rem' }}>
          Export Selected Points
        </button>
      </div>
    </div>
  );
};

export default USMap;