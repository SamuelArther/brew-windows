class ChromeCli < Formula
  desc "Control Google Chrome from the command-line"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "chrome-cli", "chrome-cli"
  end

  def caveats
    <<~EOS
      chrome-cli talks to Chrome's DevTools port. Start Chrome with
        chrome --remote-debugging-port=9222 --user-data-dir=<profile folder>
      and set CHROME_CLI_PORT if you use another port.
    EOS
  end

  test do
    assert_match "remote-debugging-port", shell_output("#{bin}/chrome-cli --help")
    port = free_port
    with_env(CHROME_CLI_PORT: port.to_s) do
      assert_match "remote-debugging-port=#{port}", shell_output("#{bin}/chrome-cli list tabs 2>&1", 1)
    end
  end
end
