class Cbfmt < Formula
  desc "Format codeblocks inside markdown and org documents"
  homepage "https://github.com/lukas-reineke/cbfmt"
  url "https://github.com/lukas-reineke/cbfmt/releases/download/v0.2.0/cbfmt_windows-x86_64-gnu_v0.2.0.zip"
  sha256 "3c1943a5d22b48e8817e7f987b7740b51e4aaf8c583deb6f7b9ed42af6e57965"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/lukas-reineke/cbfmt/archive/refs/tags/v0.2.0.tar.gz"
  end

  def install
    bin.install "cbfmt.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cbfmt --version 2>&1")
  end
end
