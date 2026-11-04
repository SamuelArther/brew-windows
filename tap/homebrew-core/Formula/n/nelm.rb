class Nelm < Formula
  desc "Kubernetes deployment tool that manages and deploys Helm Charts"
  homepage "https://github.com/werf/nelm"
  # Check stable version at https://github.com/werf/nelm/blob/main/trdl_channels.yaml
  url "https://github.com/werf/nelm/archive/refs/tags/v2.1.0.tar.gz"
  sha256 "b5a6e71cf5e936d0abb809ccf8daa5992976787b158bd7132c80e96c5b5a32c4"
  license "Apache-2.0"
  head "https://github.com/werf/nelm.git", branch: "main"

  # Track the stable channel of official Nelm binaries. This accounts for the
  # delay between a https://github.com/werf/nelm/blob/main/trdl_channels.yaml
  # update and successful release:trdl-publish workflow run.
  livecheck do
    url "https://tuf.nelm.sh/targets/channels/#{version.major}/stable"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "edd8bc9aab4149ab69ef36ea5c1d085b79f94cc419e58d85aff432ee406566f3"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ldflags = %W[-X github.com/werf/nelm/v2/pkg/common.Version=#{version}]
    system "go", "build", *std_go_args(ldflags:), "./cmd/nelm"

    generate_completions_from_executable(bin/"nelm", shell_parameter_format: :cobra)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nelm version")

    (testpath/"Chart.yaml").write <<~YAML
      apiVersion: v2
      name: mychart
      version: 1.0.0
      dependencies:
      - name: cert-manager
        version: 1.13.3
        repository: https://127.0.0.1
    YAML
    assert_match "Error: no cached repository", shell_output("#{bin}/nelm chart dependency download 2>&1", 1)
  end
end
