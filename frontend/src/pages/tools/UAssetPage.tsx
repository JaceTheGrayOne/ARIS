import { useState } from 'react';
import { UAssetForm, type UAssetFormSubmit } from '../../components/uasset/UAssetForm';
import { UAssetResultPanel } from '../../components/uasset/UAssetResultPanel';
import {
  runSerialize,
  runDeserialize,
  runInspect,
} from '../../api/uassetClient';
import type {
  UAssetSerializeResponse,
  UAssetDeserializeResponse,
  UAssetInspectResponse,
} from '../../types/contracts';
import { Panel, PanelHeader, PanelBody, Alert } from '../../components/ui';
import { FileCode } from 'lucide-react';

type UAssetResponse =
  | UAssetSerializeResponse
  | UAssetDeserializeResponse
  | UAssetInspectResponse;

export function UAssetPage() {
  const [currentResponse, setCurrentResponse] = useState<UAssetResponse | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const handleSubmit = async (payload: UAssetFormSubmit) => {
    setIsSubmitting(true);
    setSubmitError(null);

    try {
      let response: UAssetResponse;

      if (payload.kind === 'serialize') {
        response = await runSerialize(payload.request);
      } else if (payload.kind === 'deserialize') {
        response = await runDeserialize(payload.request);
      } else {
        response = await runInspect(payload.request);
      }

      setCurrentResponse(response);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setSubmitError(`Failed to submit operation: ${message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="space-y-6 module-uasset">
      {/* Page Header */}
      <div className="border-b-2 border-[var(--module-accent)] pb-4">
        <div className="flex items-center gap-3 mb-2">
          <FileCode size={32} className="text-[var(--module-accent)]" strokeWidth={2} />
          <h1 className="font-display text-3xl font-bold text-[var(--text-primary)]">
            UAsset Inspector / Converter
          </h1>
        </div>
        <p className="text-[var(--text-secondary)] ml-11">
          Serialize and deserialize Unreal Engine asset files using UAssetAPI (in-process)
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Panel>
          <PanelHeader title="Operation Settings" accent />
          <PanelBody>
            <UAssetForm onSubmit={handleSubmit} isSubmitting={isSubmitting} />
          </PanelBody>
        </Panel>

        <div className="space-y-4">
          {submitError && (
            <Alert variant="error" title="Error">
              {submitError}
            </Alert>
          )}

          <UAssetResultPanel response={currentResponse} />
        </div>
      </div>
    </div>
  );
}
