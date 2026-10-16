class Mrbayes < Formula
  desc "Bayesian inference of phylogenies and evolutionary models"
  homepage "https://nbisweden.github.io/MrBayes/"
  url "https://github.com/NBISweden/MrBayes/releases/download/v3.2.9/mb_3.2.9_win_x64.exe"
  sha256 "626e19b9872da50370303c7f07e62834b7f8b50057be56f324e84c66a12a2d70"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://nbisweden.github.io/MrBayes/download.html"
    regex(%r{href=\s*.*?/NBISweden/MrBayes/archive/v?(\d+(?:\.\d+)+[a-z]?)\.t}i)
  end

  def install
    bin.install "mb_3.2.9_win_x64.exe" => "mb.exe"
  end

  test do
    assert_path_exists bin/"mb.exe"
  end
end
