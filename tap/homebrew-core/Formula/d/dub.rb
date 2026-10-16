class Dub < Formula
  desc "Build tool for D projects"
  homepage "https://code.dlang.org/getting_started"
  url "https://github.com/dlang/dub/releases/download/v1.40.0/dub-v1.40.0-windows-x86_64.zip"
  sha256 "31a6a6982653b279fe0849c1d95b6572f2762a45167c5771b04f6651cf527c39"
  license "MIT"
  version_scheme 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://code.dlang.org/packages/dub/versions"
    regex(%r{href=.*/packages/dub/v?(\d+(?:\.\d+)+)["' >]}i)
  end

  def install
    bin.install "dub.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dub --version 2>&1")
  end
end
