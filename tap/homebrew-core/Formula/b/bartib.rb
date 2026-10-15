class Bartib < Formula
  desc "Simple timetracker for the command-line"
  homepage "https://github.com/nikolassv/bartib"
  url "https://github.com/nikolassv/bartib/releases/download/v1.1.0/bartib-x86_64-pc-windows-msvc.zip"
  sha256 "cfc9032164d96f1fe863cb7c5c22534dd295cf0363fb637bce3614dc6e050d6e"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nikolassv/bartib/archive/refs/tags/v1.1.0.tar.gz"
  end

  def install
    bin.install "bartib.exe"
  end

  test do
    bartib_file = testpath/"activities.bartib"
    touch bartib_file
    ENV["BARTIB_FILE"] = bartib_file

    system bin/"bartib", "start", "-d", "task BrewTest", "-p", "project"
    sleep 2
    system bin/"bartib", "stop"
    expected =<<~EOS.strip
      \e[1mproject.......... <1m\e[0m
          task BrewTest <1m

      \e[1mTotal............ <1m\e[0m
    EOS
    assert_equal expected, shell_output("#{bin}/bartib report").strip
  end
end
