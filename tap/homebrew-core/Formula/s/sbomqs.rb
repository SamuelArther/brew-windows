class Sbomqs < Formula
  desc "SBOM quality scorer and compliance checker"
  homepage "https://github.com/interlynk-io/sbomqs"
  url "https://github.com/interlynk-io/sbomqs/archive/refs/tags/v2.1.4.tar.gz"
  sha256 "4b85366003975fab2efb3955ad9a6a8a13c22b3b64e839b2b59943a72c00b92c"
  license "Apache-2.0"
  head "https://github.com/interlynk-io/sbomqs.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "fc86b2a654d29aaa5d019483ee2a271c967d1683d9eb56ba3c78200015641a7b"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    commit = build.head? ? Utils.git_head : tap.user
    ldflags = %W[
      -X sigs.k8s.io/release-utils/version.gitVersion=#{version}
      -X sigs.k8s.io/release-utils/version.gitCommit=#{commit}
      -X sigs.k8s.io/release-utils/version.gitTreeState=clean
      -X sigs.k8s.io/release-utils/version.buildDate=#{time.iso8601}
    ]

    system "go", "build", *std_go_args(ldflags:)

    generate_completions_from_executable bin/"sbomqs", shell_parameter_format: :cobra
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sbomqs version")

    (testpath/"sbom.cdx.json").write <<~JSON
      {
        "bomFormat": "CycloneDX",
        "specVersion": "1.6",
        "version": 1,
        "metadata": {
          "component": { "type": "application", "name": "app", "version": "1.0.0" }
        },
        "components": [
          { "type": "library", "name": "libfoo", "version": "2.0.0", "purl": "pkg:generic/libfoo@2.0.0" }
        ]
      }
    JSON

    # Only one of the two components has a PURL
    output = shell_output "#{bin}/sbomqs score --feature=comp_with_purl sbom.cdx.json"
    assert_match "5.0/10.0", output

    output = shell_output "#{bin}/sbomqs compliance --ntia sbom.cdx.json"
    assert_match "pkg:generic/libfoo@2.0.0", output
  end
end
