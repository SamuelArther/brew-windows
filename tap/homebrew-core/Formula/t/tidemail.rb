class Tidemail < Formula
  desc "TUI email client, supports IMAP, Gmail & Outlook OAuth"
  homepage "https://github.com/allisonhere/tidemail"
  url "https://github.com/allisonhere/tidemail/archive/refs/tags/v1.10.1.tar.gz"
  sha256 "ac1e7cda883f3f5366beb6e10cb18f2dfb0d477afb4a7d43a6c45b9dcd6950ad"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "05d69457e64762c17fbd38f9d3b72a32548c4aff202ed2871e4c70b7a9c89b83"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
    (man1/"tidemail.1").write Utils.safe_popen_read(bin/"tidemail", "man")
  end

  test do
    (testpath/".config/tidemail/config.toml").write <<~TOML
      [[account]]
      id = "0123456789abcdef0123456789abcdef"
      name = "gmail"
      provider = "Gmail"
      auth_method = "password"
      imap_host = "imap.gmail.com"
      imap_port = 993
      imap_tls = true
      smtp_host = "smtp.gmail.com"
      smtp_port = 587
      smtp_tls = true
      user = "example@gmail.com"
      password = "*****"
      from = "Example User <example@gmail.com>"
      sync_minutes = 0
      signature = ""
      refresh_token = ""
    TOML
    assert_match "gmail", shell_output("#{bin}/tidemail accounts list")
  end
end
