{{- define "mini-eshop.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "mini-eshop.fullname" -}}
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

{{- define "mini-eshop.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "mini-eshop.selectorLabels" -}}
app.kubernetes.io/name: {{ include "mini-eshop.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "mini-eshop.labels" -}}
helm.sh/chart: {{ include "mini-eshop.chart" . }}
{{ include "mini-eshop.selectorLabels" . }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "mini-eshop.appImage" -}}
{{- printf "%s-%s:%s" .root.Values.image.repository .service (default .root.Chart.AppVersion .root.Values.image.tag) }}
{{- end }}

{{- define "mini-eshop.waitForDeps" -}}
{{- $fullname := include "mini-eshop.fullname" .root -}}
- name: wait-for-deps
  image: {{ printf "%s:%s" .root.Values.waitForDeps.image.repository .root.Values.waitForDeps.image.tag | quote }}
  command:
    - sh
    - -c
    - |
      for target in {{ range .targets }}{{ $fullname }}-{{ . }} {{ end }}; do
        host=${target%%:*}
        port=${target##*:}
        until nc -z "$host" "$port"; do
          echo "waiting for $host:$port"
          sleep 2
        done
      done
  resources:
    {{- toYaml .root.Values.waitForDeps.resources | nindent 4 }}
{{- end }}
