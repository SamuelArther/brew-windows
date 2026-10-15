class Chsrc < Formula
  desc "Change Source for every software on every platform from the command-line"
  homepage "https://chsrc.run/"
  url "https://github.com/RubyMetric/chsrc/releases/download/v0.2.7/chsrc-x64-windows.exe"
  sha256 "d6ac2e4988bae1a0ab20711e677a85d13006e479c6e13120984e546a3373addf"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/RubyMetric/chsrc/archive/refs/tags/v0.2.7.tar.gz"
  end

  def install
    bin.install "chsrc-x64-windows.exe" => "chsrc.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/chsrc --version 2>&1")
  end
end
