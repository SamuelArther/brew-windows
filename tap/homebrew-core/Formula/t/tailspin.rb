class Tailspin < Formula
  desc "Log file highlighter"
  homepage "https://github.com/bensadeh/tailspin"
  url "https://github.com/bensadeh/tailspin/releases/download/7.0.0/tailspin-x86_64-pc-windows-msvc.zip"
  sha256 "69f6301c93288a17806a7ddbf46bb0f33f400bc4059bf12bb7b94d38b2aa9bd9"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bensadeh/tailspin/archive/refs/tags/7.0.0.tar.gz"
  end

  def install
    bin.install "tspin.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tspin --version")

    (testpath/"test.log").write("test\n")
    system bin/"tspin", "test.log"
  end
end
