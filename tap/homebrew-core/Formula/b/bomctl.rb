class Bomctl < Formula
  desc "Format-agnostic SBOM tooling for the stages between SBOM generation and analysis"
  homepage "https://github.com/bomctl/bomctl"
  url "https://github.com/bomctl/bomctl/archive/refs/tags/v0.4.3.tar.gz"
  sha256 "8d361a0813de9aa65a366a1f8333004b2fa476981481c0c90e00ccaf74a01c58"
  license "Apache-2.0"
  head "https://github.com/bomctl/bomctl.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b755ccaae0eb2c76cb3824ce157579f64f87e74358b5789875206ad6e57e5b25"
  end

  depends_on "go" => :build

  resource "homebrew-testbom", :test do
    url "https://raw.githubusercontent.com/bomctl/bomctl-playground/4712cefc49fbfbe71362aa1dd1d5dce8339b76c5/examples/bomctl-container-image/app/bomctl_0.3.0_linux_amd64.tar.gz.spdx.json"
    sha256 "01337ee051fac432f124ba4898541e84bc3d6bc97833e01136501d68e252e94e"
  end

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = "0"
    ldflags = %W[
      -X github.com/bomctl/bomctl/cmd.VersionMajor=#{version.major}
      -X github.com/bomctl/bomctl/cmd.VersionMinor=#{version.minor}
      -X github.com/bomctl/bomctl/cmd.VersionPatch=#{version.patch}
      -X github.com/bomctl/bomctl/cmd.BuildDate=#{time.iso8601}
    ]
    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable bin/"bomctl", shell_parameter_format: :cobra
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/bomctl --version")

    testpath.install resource("homebrew-testbom")

    system bin/"bomctl", "import", "--alias=testbom", "--tag=foo", "bomctl_0.3.0_linux_amd64.tar.gz.spdx.json"
    assert_match "testbom", shell_output("#{bin}/bomctl ls --tag=foo")

    system bin/"bomctl", "export", "--output-file=bom.json", "testbom"
    system bin/"bomctl", "import", "--tag=bar", "bom.json"
    assert_match "testbom", shell_output("#{bin}/bomctl ls --tag=bar")
  end
end
