class Tfproviderlint < Formula
  desc "Terraform Provider Lint Tool"
  homepage "https://github.com/bflad/tfproviderlint"
  url "https://github.com/bflad/tfproviderlint/releases/download/v0.31.0/tfproviderlint_0.31.0_windows_amd64.zip"
  sha256 "0e69b3ed169fcbf990350bd64acbb3cf78cbf1e17af4e76676750d20e9f13dde"
  license "MPL-2.0"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bflad/tfproviderlint/archive/refs/tags/v0.31.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/tfproviderlint.exe", "tfproviderlint"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tfproviderlint --version 2>&1")
  end
end
