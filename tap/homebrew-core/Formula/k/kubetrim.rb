class Kubetrim < Formula
  desc "Trim your KUBECONFIG automatically"
  homepage "https://github.com/alexellis/kubetrim"
  url "https://github.com/alexellis/kubetrim/archive/refs/tags/v0.0.3.tar.gz"
  sha256 "0721b0712d0184cabc3424019f161220e9ae4cc39f0eaf6cb5051ffb94d0ce80"
  license "MIT"
  head "https://github.com/alexellis/kubetrim.git", branch: "master"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "84c4fe34c2ce3cff11f36560d54cc34b801951e10f0b3455dd65bff71ecc9aa2"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = "-X github.com/alexellis/kubetrim/pkg.Version=#{version} -X github.com/alexellis/kubetrim/pkg.GitCommit=#{tap.user}"
    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/kubetrim --help")

    # fake k8s configuration
    (testpath/".kube/config").write <<~YAML
      apiVersion: v1
      clusters:
        - cluster:
            insecure-skip-tls-verify: true
            server: 'https://localhost:6443'
          name: test-cluster
      contexts:
        - context:
            cluster: test-cluster
            user: test-user
          name: test-context
      current-context: test-context
      kind: Config
      preferences: {}
      users:
        - name: test-user
          user:
            token: test-token
    YAML

    output = shell_output("#{bin}/kubetrim -write=false")
    assert_match "failed to connect to cluster", output
  end
end
