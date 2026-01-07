import { useState } from 'react';
import { DllInjectorForm } from '../../components/dllinjector/DllInjectorForm';
import { DllInjectorResultPanel } from '../../components/dllinjector/DllInjectorResultPanel';
import { runInject, runEject } from '../../api/dllInjectorClient';
import type {
  DllInjectCommandDto,
  DllEjectCommandDto,
  DllInjectResponse,
  DllEjectResponse,
} from '../../types/contracts';
import { Panel, PanelHeader, PanelBody, Alert } from '../../components/ui';
import { Syringe } from 'lucide-react';

export function DllInjectorPage() {
  const [injectResponse, setInjectResponse] = useState<DllInjectResponse | null>(null);
  const [ejectResponse, setEjectResponse] = useState<DllEjectResponse | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const handleInject = async (command: DllInjectCommandDto) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const response = await runInject(command);
      setInjectResponse(response);
      setEjectResponse(null);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setSubmitError(`Failed to inject DLL: ${message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleEject = async (command: DllEjectCommandDto) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const response = await runEject(command);
      setEjectResponse(response);
      setInjectResponse(null);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setSubmitError(`Failed to eject DLL: ${message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="space-y-6 module-injector">
      {/* Page Header */}
      <div className="border-b-2 border-[var(--module-accent)] pb-4">
        <div className="flex items-center gap-3 mb-2">
          <Syringe size={32} className="text-[var(--module-accent)]" strokeWidth={2} />
          <h1 className="font-display text-3xl font-bold text-[var(--text-primary)]">
            DLL Injector
          </h1>
        </div>
        <p className="text-[var(--text-secondary)] ml-11">
          Inject or eject DLL libraries into running game processes for runtime modifications
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Panel>
          <PanelHeader title="Injection Settings" accent />
          <PanelBody>
            <DllInjectorForm
              onInject={handleInject}
              onEject={handleEject}
              isSubmitting={isSubmitting}
            />
          </PanelBody>
        </Panel>

        <div className="space-y-4">
          {submitError && (
            <Alert variant="error" title="Error">
              {submitError}
            </Alert>
          )}

          <DllInjectorResultPanel
            injectResponse={injectResponse}
            ejectResponse={ejectResponse}
          />
        </div>
      </div>
    </div>
  );
}
