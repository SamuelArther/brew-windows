class Shellcheck < Formula
  desc "Static analysis and lint tool, for (ba)sh scripts"
  homepage "https://www.shellcheck.net/"
  url "https://github.com/koalaman/shellcheck/releases/download/v0.11.0/shellcheck-v0.11.0.zip"
  sha256 "8a4e35ab0b331c85d73567b12f2a444df187f483e5079ceffa6bda1faa2e740e"
  license "GPL-3.0-or-later"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "shellcheck.exe"
  end

  test do
    sh = testpath/"test.sh"
    sh.write <<~SH
      for f in $(ls *.wav)
      do
        echo "$f"
      done
    SH
    assert_match "[SC2045]", shell_output("#{bin}/shellcheck -f gcc #{sh}", 1)
  end
end
