class Tfprovidercheck < Formula
  desc "CLI to prevent malicious Terraform Providers from being executed"
  homepage "https://github.com/suzuki-shunsuke/tfprovidercheck"
  url "https://github.com/suzuki-shunsuke/tfprovidercheck/releases/download/v1.0.7/tfprovidercheck_windows_amd64.zip"
  sha256 "25669f68fc1741176ef2d1f4a4eafe71da72e3e93f4a1850bae969e642348bef"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/suzuki-shunsuke/tfprovidercheck/archive/refs/tags/v1.0.7.tar.gz"
  end

  def install
    bin.install "tfprovidercheck.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tfprovidercheck --version 2>&1")
  end
end
