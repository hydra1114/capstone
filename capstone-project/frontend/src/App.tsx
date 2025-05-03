// filepath: /c:/Users/colen/OneDrive/Grad School/CSCI 8910 - Capstone/Repo/capstone-project/frontend/src/App.tsx
import React from 'react';
import logo from './logo.svg';
import './App.css';
import USMap from './USMap';

const App: React.FC = () => {
  return (
    <div className="App">
      <main>
        <USMap />
      </main>
    </div>
  );
};

export default App;