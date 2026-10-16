class Tfswitch < Formula
  desc "Command-line tool to switch between Terraform versions"
  homepage "https://tfswitch.warrensbox.com"
  url "https://github.com/warrensbox/terraform-switcher/releases/download/v1.20.0/terraform-switcher_v1.20.0_windows_amd64.zip"
  sha256 "93c096b0653435de0ca2df670684e8451d8641ee79c2182629e89c69ac403cde"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/warrensbox/terraform-switcher/archive/refs/tags/v1.20.0.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "tfswitch.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tfswitch --version")

    (testpath/"versions.tf").write <<~HCL
      terraform {
        required_version = "~> 1.5.0"
      }
    HCL
    assert_match 'Version "1.5.7" matches requirement "~> 1.5.0"',
                 shell_output("#{bin}/tfswitch --match-version-requirement 1.5.7 2>&1")
    assert_match 'Version "1.6.0" mismatches requirement "~> 1.5.0"',
                 shell_output("#{bin}/tfswitch --match-version-requirement 1.6.0 2>&1", 2)
  end
end
