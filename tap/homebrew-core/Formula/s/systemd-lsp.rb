class SystemdLsp < Formula
  desc "Language server for systemd unit files"
  homepage "https://github.com/JFryy/systemd-lsp"
  url "https://github.com/JFryy/systemd-lsp/archive/refs/tags/v2026.09.28.tar.gz"
  sha256 "d9fe3b5b81eb6d9363e2ed324909868810ffd2d7a49eb24233524d292db28de8"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "1d546d06f475faf6c743a0b72750cf8441c948bc06946ae853f4d0859c92c1df"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    (testpath/"test.service").write <<~EOS
      [Service]
      ExecTest=brew
    EOS
    assert_match "Unknown directive 'ExecTest' in [Service] section",
      shell_output("#{bin}/systemd-lsp test.service")
  end
end
