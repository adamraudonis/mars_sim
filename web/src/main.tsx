import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { AppRunner } from './runner';
import { App } from './ui/App';
import './ui/theme.css';

const root = createRoot(document.getElementById('root')!);
root.render(
  <div className="loading">
    <div className="mark" />
    <div>LOADING SOURCED PARAMETERS…</div>
  </div>,
);

AppRunner.create().then((runner) => {
  root.render(
    <StrictMode>
      <App runner={runner} />
    </StrictMode>,
  );
});
