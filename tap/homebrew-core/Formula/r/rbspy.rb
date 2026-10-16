class Rbspy < Formula
  desc "Sampling profiler for Ruby"
  homepage "https://rbspy.github.io/"
  url "https://github.com/rbspy/rbspy/releases/download/v0.53.0/rbspy-x86_64-pc-windows-msvc.exe.zip"
  sha256 "10dbface534d48c75c3eae4a1d4a1a66466faed1db2265cea429e5d671ea8683"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/rbspy/rbspy/archive/refs/tags/v0.53.0.tar.gz"
  end

  def install
    bin.install "rbspy-x86_64-pc-windows-msvc.exe" => "rbspy.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rbspy --version 2>&1")
  end
end
