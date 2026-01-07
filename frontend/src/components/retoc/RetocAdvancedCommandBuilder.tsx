import { useState, useEffect } from 'react';
import { getRetocSchema } from '../../api/retocClient';
import type { RetocCommandSchemaResponse, RetocCommandDefinition, RetocCommandFieldDefinition, RetocFieldUiHint } from '../../types/contracts';
import { Panel, PanelHeader, PanelBody, Field, Input, Select, Alert } from '../ui';

interface RetocAdvancedCommandBuilderProps {
  onFieldChange: (field: string, value: string | number | boolean) => void;
  values: Record<string, any>;
  errors: Record<string, string>;
}

export function RetocAdvancedCommandBuilder({
  onFieldChange,
  values,
  errors,
}: RetocAdvancedCommandBuilderProps) {
  const [schema, setSchema] = useState<RetocCommandSchemaResponse | null>(null);
  const [schemaError, setSchemaError] = useState<string | null>(null);
  const [selectedCommand, setSelectedCommand] = useState<RetocCommandDefinition | null>(null);

  useEffect(() => {
    loadSchema();
  }, []);

  useEffect(() => {
    if (schema && values.commandType) {
      const command = schema.commands.find((c) => c.commandType === values.commandType);
      setSelectedCommand(command || null);
    } else {
      setSelectedCommand(null);
    }
  }, [values.commandType, schema]);

  const loadSchema = async () => {
    try {
      const response = await getRetocSchema();
      setSchema(response);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Unknown error';
      setSchemaError(`Failed to load schema: ${message}`);
    }
  };

  const handleCommandChange = (commandType: string) => {
    onFieldChange('commandType', commandType);
    // Clear optional fields when command changes
    if (schema) {
      const globalFields = schema.globalOptions.map((f) => f.fieldName);
      globalFields.forEach((field) => {
        if (field !== 'InputPath' && field !== 'OutputPath') {
          onFieldChange(field, '');
        }
      });
    }
  };

  const renderField = (fieldDef: RetocCommandFieldDefinition, isRequired: boolean, uiHint?: RetocFieldUiHint) => {
    const { fieldName, label, fieldType, helpText, enumValues, minValue, maxValue } = fieldDef;
    const value = values[fieldName] || '';
    const error = errors[fieldName];

    switch (fieldType) {
      case 'Path':
      case 'String': {
        // Determine path placeholder and help text based on UI hint
        let placeholder = '';
        let pathHelpText = helpText || '';
        const isFolder = uiHint?.pathKind === 'folder';
        const isFile = uiHint?.pathKind === 'file';

        if (fieldType === 'Path') {
          if (isFolder) {
            placeholder = 'C:\\path\\to\\folder';
            if (!pathHelpText) {
              pathHelpText = 'Select a folder';
            }
          } else if (isFile) {
            if (uiHint?.extensions?.length) {
              placeholder = `C:\\path\\to\\file (${uiHint.extensions.join(', ')})`;
              pathHelpText = pathHelpText
                ? `${pathHelpText} — Expected: ${uiHint.extensions.join(', ')}`
                : `Select a file (${uiHint.extensions.join(', ')})`;
            } else {
              placeholder = 'C:\\path\\to\\file';
              if (!pathHelpText) {
                pathHelpText = 'Select a file';
              }
            }
          } else {
            placeholder = 'C:\\path\\to\\file';
          }
        }

        return (
          <Field
            key={fieldName}
            label={label}
            required={isRequired}
            error={error}
            help={pathHelpText || undefined}
          >
            <Input
              type="text"
              value={value}
              onChange={(e) => onFieldChange(fieldName, e.target.value)}
              error={!!error}
              placeholder={placeholder}
            />
          </Field>
        );
      }

      case 'Integer':
        return (
          <Field
            key={fieldName}
            label={label}
            required={isRequired}
            error={error}
            help={helpText || undefined}
          >
            <Input
              type="number"
              value={value}
              onChange={(e) => onFieldChange(fieldName, parseInt(e.target.value, 10))}
              error={!!error}
              min={minValue ?? undefined}
              max={maxValue ?? undefined}
            />
          </Field>
        );

      case 'Enum':
        return (
          <Field
            key={fieldName}
            label={label}
            required={isRequired}
            error={error}
            help={helpText || undefined}
          >
            <Select
              value={value}
              onChange={(e) => onFieldChange(fieldName, e.target.value)}
              error={!!error}
            >
              <option value="">-- Select {label} --</option>
              {enumValues?.map((enumValue) => (
                <option key={enumValue} value={enumValue}>
                  {enumValue}
                </option>
              ))}
            </Select>
          </Field>
        );

      case 'Boolean':
        return (
          <Field key={fieldName} label="" help={helpText || undefined}>
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={!!value}
                onChange={(e) => onFieldChange(fieldName, e.target.checked)}
                className="w-4 h-4 text-[var(--module-accent)] bg-[var(--bg-input)] border-[var(--border-default)] rounded focus:ring-2 focus:ring-[var(--module-accent)]"
              />
              <span className="text-[var(--text-primary)] text-sm">{label}</span>
            </label>
          </Field>
        );

      default:
        return null;
    }
  };

  if (schemaError) {
    return (
      <Panel>
        <PanelHeader title="Advanced Mode" />
        <PanelBody>
          <Alert variant="error" title="Schema Error">
            {schemaError}
          </Alert>
        </PanelBody>
      </Panel>
    );
  }

  if (!schema) {
    return (
      <Panel>
        <PanelHeader title="Advanced Mode" />
        <PanelBody>
          <div className="text-[var(--text-muted)]">Loading schema...</div>
        </PanelBody>
      </Panel>
    );
  }

  const requiredFieldDefs = selectedCommand
    ? schema.globalOptions.filter((f) => selectedCommand.requiredFields.includes(f.fieldName))
    : [];

  const optionalFieldDefs = selectedCommand
    ? schema.globalOptions.filter((f) => selectedCommand.optionalFields.includes(f.fieldName))
    : [];

  const booleanFieldDefs = schema.globalOptions.filter((f) => f.fieldType === 'Boolean');

  return (
    <Panel>
      <PanelHeader
        title="Advanced Mode"
        subtitle="Full Retoc command builder"
      />
      <PanelBody>
        <div className="space-y-4">
          {/* Command Selector */}
          <Field label="Command" required>
            <Select
              value={values.commandType || ''}
              onChange={(e) => handleCommandChange(e.target.value)}
            >
              <option value="">-- Select Command --</option>
              {schema.commands.map((cmd) => (
                <option key={cmd.commandType} value={cmd.commandType}>
                  {cmd.displayName}
                </option>
              ))}
            </Select>
          </Field>

          {selectedCommand && (
            <>
              <div className="text-[var(--text-secondary)] text-sm border-l-2 border-[var(--module-accent)] pl-3">
                {selectedCommand.description}
              </div>

              {/* Required Fields */}
              {requiredFieldDefs.length > 0 && (
                <div className="space-y-4">
                  <h3 className="text-[var(--text-primary)] font-semibold text-sm">
                    Required Fields
                  </h3>
                  {requiredFieldDefs.map((fieldDef) =>
                    renderField(fieldDef, true, selectedCommand.fieldUiHints?.[fieldDef.fieldName])
                  )}
                </div>
              )}

              {/* Optional Fields */}
              {optionalFieldDefs.length > 0 && (
                <div className="space-y-4">
                  <h3 className="text-[var(--text-primary)] font-semibold text-sm">
                    Optional Fields
                  </h3>
                  {optionalFieldDefs.map((fieldDef) =>
                    renderField(fieldDef, false, selectedCommand.fieldUiHints?.[fieldDef.fieldName])
                  )}
                </div>
              )}

              {/* Boolean Flags */}
              {booleanFieldDefs.length > 0 && (
                <div className="space-y-3">
                  <h3 className="text-[var(--text-primary)] font-semibold text-sm">
                    Options
                  </h3>
                  {booleanFieldDefs.map((fieldDef) => renderField(fieldDef, false))}
                </div>
              )}
            </>
          )}
        </div>
      </PanelBody>
    </Panel>
  );
}
