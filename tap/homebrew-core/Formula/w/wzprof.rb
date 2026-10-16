class Wzprof < Formula
  desc "Profiling for Wazero"
  homepage "https://github.com/dispatchrun/wzprof"
  url "https://github.com/dispatchrun/wzprof/releases/download/v0.2.0/wzprof_0.2.0_windows_amd64.tar.gz"
  sha256 "11fab5406a00e83d97f1bf81cdab97754c7e123dc1942723b818c918bd5b17ee"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dispatchrun/wzprof/archive/refs/tags/v0.2.0.tar.gz"
  end

  def install
    bin.install "wzprof.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wzprof --version 2>&1")
  end
end
