class Genact < Formula
  desc "Nonsense activity generator"
  homepage "https://svenstaro.github.io/genact/"
  url "https://github.com/svenstaro/genact/releases/download/v1.6.0/genact-1.6.0-x86_64-pc-windows-msvc.exe"
  sha256 "3cb0c7059786e8110d837675f0ec9a2178609c56f3b1aa1d2b773f8cb9bcfb61"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/svenstaro/genact/archive/refs/tags/v1.6.0.tar.gz"
  end

  def install
    bin.install "genact-1.6.0-x86_64-pc-windows-msvc.exe" => "genact.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/genact --version 2>&1")
  end
end
