class SystemdLsp < Formula
  desc "Language server for systemd unit files"
  homepage "https://github.com/JFryy/systemd-lsp"
  url "https://github.com/JFryy/systemd-lsp/archive/refs/tags/v2026.11.02.tar.gz"
  sha256 "2f8e31311e2c8b93a6e02d7be2b81691ffc02066fdf1a6d41529e7b53a4e80aa"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "2d53f156e9d33f245e40c73c251845601d0f781f182d62c806d5a65250da646d"
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
