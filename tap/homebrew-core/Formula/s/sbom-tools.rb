class SbomTools < Formula
  desc "SBOM multi-tool: diff, quality, compliance, vulnerability tracking, VEXes, etc."
  homepage "https://sbom.tools"
  url "https://github.com/sbom-tool/sbom-tools/archive/refs/tags/v0.4.0.tar.gz"
  sha256 "249607a385ae5feeb91637c047827480983b6580fd56cdbeae605de0640b9582"
  license "MIT"
  head "https://github.com/sbom-tool/sbom-tools.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "91e544cd4f55c6cde400d0d6e4c74b6ad43852d5dc925bae6c238bba4f952539"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args

    generate_completions_from_executable bin/"sbom-tools", "completions",
                                         shells: [:bash, :zsh, :fish, :pwsh]
    (man1/"sbom-tools.1").write Utils.safe_popen_read(bin/"sbom-tools", "man")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sbom-tools --version") unless build.head?

    (testpath/"old.cdx.json").write <<~JSON
      {
        "bomFormat": "CycloneDX",
        "specVersion": "1.5",
        "components": [
          { "type": "library", "name": "lodash", "version": "4.17.20" }
        ]
      }
    JSON
    (testpath/"new.cdx.json").write <<~JSON
      {
        "bomFormat": "CycloneDX",
        "specVersion": "1.5",
        "components": [
          { "type": "library", "name": "lodash", "version": "4.17.21" },
          { "type": "library", "name": "axios", "version": "1.6.0" }
        ],
        "vulnerabilities": [
          { "id": "CVE-2024-39338", "affects": [{ "ref": "axios" }] }
        ]
      }
    JSON

    output = shell_output("#{bin}/sbom-tools diff old.cdx.json new.cdx.json --fail-on-vuln --output=json", 2)
    expected_component_diff = { "added" => 1, "removed" => 0, "modified" => 1 }
    assert_equal expected_component_diff, JSON.parse(output)["summary"]["components"]
    assert_equal 1, JSON.parse(output)["summary"]["vulnerabilities"]["introduced"]

    output = shell_output("#{bin}/sbom-tools quality old.cdx.json --profile=security --min-score=90", 1)
    assert_match "Grade: F", output

    output = shell_output("#{bin}/sbom-tools validate new.cdx.json --standard=ntia", 1)
    assert_match "NON-COMPLIANT", output
  end
end
