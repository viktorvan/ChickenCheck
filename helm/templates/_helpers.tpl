{{/* ConnectionString */}}
{{- define "helpers.connectionString" -}}
"User ID={{ .Values.databaseUser }};Password={{ .Values.databasePassword }};Host=postgres-service.svc.cluster.local;Port=5432;Database={{ .Values.databaseName }};Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;Connection Lifetime=0;"
{{- end -}}
