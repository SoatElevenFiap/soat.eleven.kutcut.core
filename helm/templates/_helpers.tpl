{{/*
Expand the name of the chart.
*/}}
{{- define "kutcut-api.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "kutcut-api.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart label.
*/}}
{{- define "kutcut-api.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels applied to all resources.
*/}}
{{- define "kutcut-api.labels" -}}
helm.sh/chart: {{ include "kutcut-api.chart" . }}
{{ include "kutcut-api.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels (used in Deployment matchLabels and Service selector).
*/}}
{{- define "kutcut-api.selectorLabels" -}}
app.kubernetes.io/name: {{ include "kutcut-api.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Name of the Secret that holds sensitive configuration.
*/}}
{{- define "kutcut-api.secretName" -}}
{{- printf "%s-secret" (include "kutcut-api.fullname" .) }}
{{- end }}

{{/*
Name of the ConfigMap that holds non-sensitive configuration.
*/}}
{{- define "kutcut-api.configMapName" -}}
{{- printf "%s-config" (include "kutcut-api.fullname" .) }}
{{- end }}
