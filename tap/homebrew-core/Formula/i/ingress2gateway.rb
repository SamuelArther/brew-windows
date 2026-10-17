class Ingress2gateway < Formula
  desc "Convert Kubernetes Ingress resources to Kubernetes Gateway API resources"
  homepage "https://github.com/kubernetes-sigs/ingress2gateway"
  url "https://github.com/kubernetes-sigs/ingress2gateway/archive/refs/tags/v1.2.0.tar.gz"
  sha256 "881f2599b39d29f14c07e1ae12c60ffbbe636ae07a7ca6e1491d70bb8ab37c5f"
  license "Apache-2.0"
  head "https://github.com/kubernetes-sigs/ingress2gateway.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "655d2e51e843769fd2d1d4777ccac0c1e5b12849b11c253b3f9a858e5b906eb4"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/kubernetes-sigs/ingress2gateway/pkg/i2gw.Version=#{version}"
    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable(bin/"ingress2gateway", shell_parameter_format: :cobra)
  end

  test do
    (testpath/"test.yml").write <<~YAML
      apiVersion: networking.k8s.io/v1
      kind: Ingress
      metadata:
        name: foo
        namespace: bar
        annotations:
          cert-manager.io/cluster-issuer: "letsencrypt-prod"
        labels:
          name: foo
      spec:
        ingressClassName: nginx
        rules:
        - host: foo.bar
          http:
            paths:
            - pathType: Prefix
              path: "/"
              backend:
                service:
                  name: foo-bar
                  port:
                    number: 443
    YAML

    expected = <<~YAML
      apiVersion: gateway.networking.k8s.io/v1
      kind: Gateway
      metadata:
        annotations:
          gateway.networking.k8s.io/generator: ingress2gateway-#{version}
        name: nginx
        namespace: bar
      spec:
        gatewayClassName: nginx
        listeners:
        - hostname: foo.bar
          name: foo-bar-http
          port: 80
          protocol: HTTP
      ---
      apiVersion: gateway.networking.k8s.io/v1
      kind: HTTPRoute
      metadata:
        annotations:
          gateway.networking.k8s.io/generator: ingress2gateway-#{version}
        name: foo-foo-bar
        namespace: bar
      spec:
        hostnames:
        - foo.bar
        parentRefs:
        - name: nginx
        rules:
        - backendRefs:
          - name: foo-bar
            port: 443
          matches:
          - path:
              type: PathPrefix
              value: /
      status:
        parents: []
    YAML

    output = shell_output("#{bin}/ingress2gateway print --providers ingress-nginx --input-file #{testpath}/test.yml")
    assert_equal expected.chomp, output.chomp
  end
end
