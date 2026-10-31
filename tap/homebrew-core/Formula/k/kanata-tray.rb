class KanataTray < Formula
  desc "System tray for kanata keyboard remapper"
  homepage "https://github.com/rszyma/kanata-tray"
  url "https://github.com/rszyma/kanata-tray/archive/refs/tags/v0.8.0.tar.gz"
  sha256 "18d17493d20f6a06db31131c41059d2a8224494479fdf98498eb76c0fab70885"
  license "GPL-3.0-or-later"
  head "https://github.com/rszyma/kanata-tray.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "aaec9aadbccaf0336b861e64ef7a58c6f07d11d98fdda758b98594311c5d1133"
  end

  depends_on "go" => :build
  depends_on "pkgconf" => :build

  on_linux do
    depends_on "glib"
    depends_on "gtk+3"
    depends_on "libayatana-appindicator"
  end

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    # Workaround to avoid patchelf corruption when cgo is required
    if OS.linux? && Hardware::CPU.arm64?
      ENV["CGO_ENABLED"] = "1"
      ENV["GO_EXTLINK_ENABLED"] = "1"
      ENV.append "GOFLAGS", "-buildmode=pie"
    end

    ldflags = " -X main.buildVersion=#{version} -X main.buildHash=#{tap.user} -X main.buildDate=#{time.iso8601}"
    system "go", "build", *std_go_args(ldflags:)
  end

  test do
    ENV["KANATA_TRAY_LOG_DIR"] = testpath

    assert_match version.to_s, shell_output("#{bin}/kanata-tray --version 2>&1", 1)

    pid = spawn bin/"kanata-tray"
    sleep 2
    assert_match "Creating it and populating with the default icons", (testpath/"kanata_tray_lastrun.log").read
  ensure
    Process.kill("TERM", pid)
    Process.wait(pid)
  end
end
