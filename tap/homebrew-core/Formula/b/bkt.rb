class Bkt < Formula
  desc "CLI utility for caching the output of subprocesses"
  homepage "https://github.com/dimo414/bkt"
  url "https://github.com/dimo414/bkt/releases/download/0.8.2/bkt.v0.8.2.x86_64-pc-windows-msvc.zip"
  sha256 "a825dbc7514366d89d8b68b0adcd7fa7be7625bfb5a69c06886e27ef63f7b081"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dimo414/bkt/archive/refs/tags/0.8.2.tar.gz"
  end

  def install
    bin.install "bkt.exe"
  end

  test do
    # Make sure date output is cached between runs
    output1 = shell_output("#{bin}/bkt --ttl=1m -- date +%s.%N")
    sleep(1)
    assert_equal output1, shell_output("#{bin}/bkt --ttl=1m -- date +%s.%N")
  end
end
