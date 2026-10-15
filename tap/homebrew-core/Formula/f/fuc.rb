class Fuc < Formula
  desc "Modern, performance focused unix commands"
  homepage "https://github.com/supercilex/fuc"
  url "https://github.com/SUPERCILEX/fuc/releases/download/3.2.1/x86_64-pc-windows-msvc-cpz.exe"
  sha256 "68494198f85d9991d50d1a61f300589985e4e4c1e030a2790995ea8136ec59e8"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/supercilex/fuc/archive/refs/tags/3.2.1.tar.gz"
  end

  def install
    bin.install "x86_64-pc-windows-msvc-cpz.exe" => "cpz.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cpz --version 2>&1")
  end
end
