class KubeLinter < Formula
  desc "Static analysis tool for Kubernetes YAML files and Helm charts"
  homepage "https://docs.kubelinter.io/"
  url "https://github.com/stackrox/kube-linter/archive/refs/tags/v0.8.3.tar.gz"
  sha256 "e548670732ec47e34308abc96755c2cffeeaa1d942222167669709d612caa7d1"
  license "Apache-2.0"
  head "https://github.com/stackrox/kube-linter.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "5d2a3b64c2f5ef683f0e01dd4095be2a78e30f58336062029595dd6552977afa"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = "0"
    ldflags = "-X golang.stackrox.io/kube-linter/internal/version.version=#{version}"
    system "go", "build", *std_go_args(ldflags:), "./cmd/kube-linter"

    generate_completions_from_executable(bin/"kube-linter", shell_parameter_format: :cobra)
  end

  test do
    (testpath/"pod.yaml").write <<~YAML
      apiVersion: v1
      kind: Pod
      metadata:
        name: homebrew-demo
      spec:
        securityContext:
          runAsUser: 1000
          runAsGroup: 3000
          fsGroup: 2000
        containers:
        - name: homebrew-test
          image: busybox:stable
          resources:
            limits:
              memory: "128Mi"
              cpu: "500m"
            requests:
              memory: "64Mi"
              cpu: "250m"
          securityContext:
            readOnlyRootFilesystem: true
    YAML

    # Lint pod.yaml for default errors
    assert_match "No lint errors found!", shell_output("#{bin}/kube-linter lint pod.yaml 2>&1").chomp
    assert_equal version.to_s, shell_output("#{bin}/kube-linter version").chomp
  end
end
