class Brename < Formula
  desc "Cross-platform command-line tool for safe batch renaming via regular expressions"
  homepage "https://github.com/shenwei356/brename"
  url "https://github.com/shenwei356/brename/releases/download/v2.14.0/brename_windows_amd64.exe.tar.gz"
  sha256 "020ce7a897baadbd19ac27bf28e6ffb3b4024ee09fa1b46534c5c75881da1041"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/shenwei356/brename/archive/refs/tags/v2.14.0.tar.gz"
  end

  def install
    bin.install "brename.exe"
  end

  test do
    (1..9).each do |n|
      (testpath/"Homebrew-#{n}.txt").write n.to_s
    end

    system bin/"brename", "-p", "Homebrew-(\\d+).txt", "-r", "$1.txt", "-v", "0"

    (1..9).each do |n|
      assert_equal n.to_s, (testpath/"#{n}.txt").read
      refute_path_exists testpath/"Homebrew-#{n}.txt"
    end
  end
end
