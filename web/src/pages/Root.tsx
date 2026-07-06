import type { AppRunner } from '../runner';
import { App } from '../ui/App';
import { useRoute } from './nav';
import { DocShell } from './DocShell';
import { AssetsPage } from './AssetsPage';
import { SystemsPage } from './SystemsPage';
import { LogicPage } from './LogicPage';
import { TradesPage } from './TradesPage';

/** Hash router: the sim lives at #/, the document pages hang off it and share the runner. */
export function Root({ runner }: { runner: AppRunner }) {
  const route = useRoute();

  if (route === 'sim') return <App runner={runner} />;

  return (
    <DocShell runner={runner} route={route}>
      {route === 'assets' && <AssetsPage runner={runner} />}
      {route === 'systems' && <SystemsPage runner={runner} />}
      {route === 'logic' && <LogicPage runner={runner} />}
      {route === 'trades' && <TradesPage />}
    </DocShell>
  );
}
