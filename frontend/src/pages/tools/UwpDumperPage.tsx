import { useState } from 'react';
import { UwpDumperForm } from '../../components/uwpdumper/UwpDumperForm';
import { UwpDumperResultPanel } from '../../components/uwpdumper/UwpDumperResultPanel';
import { runDump } from '../../api/uwpDumperClient';
import type { UwpDumpCommandDto, UwpDumpResponse } from '../../types/contracts';
import { Panel, PanelHeader, PanelBody, Alert } from '../../components/ui';
import { Shield } from 'lucide-react';

export function UwpDumperPage() {
  const [currentResponse, setCurrentResponse] = useState<UwpDumpResponse | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const handleSubmit = async (command: UwpDumpCommandDto) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      const response = await runDump(command);
      setCurrentResponse(response);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setSubmitError(`Failed to submit dump operation: ${message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="space-y-6 module-uwp">
      {/* Page Header */}
      <div className="border-b-2 border-[var(--module-accent)] pb-4">
        <div className="flex items-center gap-3 mb-2">
          <Shield size={32} className="text-[var(--module-accent)]" strokeWidth={2} />
          <h1 className="font-display text-3xl font-bold text-[var(--text-primary)]">
            UWP Dumper
          </h1>
        </div>
        <p className="text-[var(--text-secondary)] ml-11">
          Dump Universal Windows Platform application packages and extract protected files
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Panel>
          <PanelHeader title="Dump Settings" accent />
          <PanelBody>
            <UwpDumperForm onSubmit={handleSubmit} isSubmitting={isSubmitting} />
          </PanelBody>
        </Panel>

        <div className="space-y-4">
          {submitError && (
            <Alert variant="error" title="Error">
              {submitError}
            </Alert>
          )}

          <UwpDumperResultPanel response={currentResponse} />
        </div>
      </div>
    </div>
  );
}
