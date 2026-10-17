class YubikeyAgent < Formula
  desc "Seamless ssh-agent for YubiKeys and other PIV tokens"
  homepage "https://github.com/FiloSottile/yubikey-agent"
  url "https://github.com/FiloSottile/yubikey-agent/archive/refs/tags/v0.1.6.tar.gz"
  sha256 "f156d089376772a34d2995f8261d821369a96a248ab586d27e3be0d9b72d7426"
  license "BSD-3-Clause"
  head "https://github.com/FiloSottile/yubikey-agent.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "4e8aff953aa5dfef555cf40f294c7e1b1b13672c79513ccbfcd85c391933f709"
  end

  depends_on "go" => :build
  depends_on "pkgconf" => :build

  uses_from_macos "pcsc-lite"

  on_linux do
    depends_on "pinentry"
  end

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = "1" if OS.linux? && Hardware::CPU.arm?
    system "go", "build", *std_go_args(ldflags: "-X main.Version=v#{version}")
    (var/"run").mkpath
  end

  def caveats
    <<~EOS
      To use this SSH agent, set this variable in your ~/.zshrc and/or ~/.bashrc:
        export SSH_AUTH_SOCK="#{var}/run/yubikey-agent.sock"
    EOS
  end

  service do
    run [opt_bin/"yubikey-agent", "-l", var/"run/yubikey-agent.sock"]
    keep_alive true
    log_path var/"log/yubikey-agent.log"
    error_log_path var/"log/yubikey-agent.log"
  end

  test do
    socket = testpath/"yubikey-agent.sock"
    spawn bin/"yubikey-agent", "-l", socket
    sleep 1
    assert_path_exists socket
  end
end
