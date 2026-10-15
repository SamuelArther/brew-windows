class Kustomize < Formula
  desc "Template-free customization of Kubernetes YAML manifests"
  homepage "https://github.com/kubernetes-sigs/kustomize"
  version "5.8.3"
  url "https://github.com/kubernetes-sigs/kustomize/releases/download/kustomize%2Fv5.8.3/kustomize_v5.8.3_windows_amd64.zip"
  sha256 "763c4040ff01435bfce39661072452ebdf1cf2808bb91f5708ab4420834fbd42"
  license "Apache-2.0"

  def install
    bin.install "kustomize.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/kustomize version")

    (testpath/"kustomization.yaml").write <<~YAML
      resources:
      - service.yaml
      patches:
      - path: patch.yaml
    YAML
    (testpath/"patch.yaml").write <<~YAML
      apiVersion: v1
      kind: Service
      metadata:
        name: brew-test
      spec:
        selector:
          app: foo
    YAML
    (testpath/"service.yaml").write <<~YAML
      apiVersion: v1
      kind: Service
      metadata:
        name: brew-test
      spec:
        type: LoadBalancer
    YAML
    output = shell_output("#{bin}/kustomize build #{testpath}")
    assert_match "type: LoadBalancer", output
  end
end
