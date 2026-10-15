class Otree < Formula
  desc "Command-line tool to view objects (JSON/YAML/TOML) in TUI tree widget"
  homepage "https://github.com/fioncat/otree"
  url "https://github.com/fioncat/otree/releases/download/v0.7.1/otree-x86_64-pc-windows-msvc.zip"
  sha256 "25a2de25b683770b2496650717addb39ffed09391039f514d3ecf7704dd9ceed"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/fioncat/otree/archive/refs/tags/v0.7.1.tar.gz"
  end

  def install
    bin.install "otree.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/otree --version 2>&1")
  end
end
