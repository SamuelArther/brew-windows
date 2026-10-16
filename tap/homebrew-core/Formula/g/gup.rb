class Gup < Formula
  desc "Update binaries installed by go install"
  homepage "https://github.com/nao1215/gup"
  url "https://github.com/nao1215/gup/releases/download/v1.10.3/gup_1.10.3_windows_amd64.zip"
  sha256 "8cf844d4e01b6fab3edfde1bda7aa2fa8ac34a4f2e116ec5bb775be41244a0e1"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nao1215/gup/archive/refs/tags/v1.10.3.tar.gz"
  end

  def install
    bin.install "gup.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gup --version 2>&1")
  end
end
