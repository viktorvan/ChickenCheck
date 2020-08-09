{{/* ConnectionString */}}
{{- define "helpers.connectionString" -}}
"Data Source={{ .Values.databasePath }}/{{ .Values.databaseName }}"
{{- end -}}
