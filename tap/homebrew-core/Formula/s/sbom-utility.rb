class SbomUtility < Formula
  desc "Tool to validate, analyze, query and edit Software Bills of Materials (SBOMs)"
  homepage "https://github.com/CycloneDX/sbom-utility"
  url "https://github.com/CycloneDX/sbom-utility/releases/download/v0.19.2/sbom-utility-v0.19.2-windows-amd64.zip"
  sha256 "567d88c04d193abaf3f17c5d75f3fd59e1945c0ce0b94734ab7b282966c43b54"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/CycloneDX/sbom-utility/archive/refs/tags/v0.19.2.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/sbom-utility.exe", "sbom-utility"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sbom-utility version 2>&1")
  end
end
