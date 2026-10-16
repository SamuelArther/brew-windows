class Lune < Formula
  desc "Standalone Luau script runtime"
  homepage "https://lune-org.github.io/docs"
  url "https://github.com/lune-org/lune/releases/download/v0.11.0/lune-0.11.0-windows-x86_64.zip"
  sha256 "05de2b841a31653531611d0217aac553f165bb69732225f95056651e2d668f3b"
  license "MPL-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/lune-org/lune/archive/refs/tags/v0.11.0.tar.gz"
  end

  def install
    bin.install "lune.exe"
  end

  test do
    (testpath/"test.lua").write("print(2 + 2)")
    assert_equal "4", shell_output("#{bin}/lune run test.lua").chomp
  end
end
