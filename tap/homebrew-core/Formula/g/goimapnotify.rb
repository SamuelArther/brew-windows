class Goimapnotify < Formula
  desc "Execute scripts on IMAP mailbox changes using IDLE"
  homepage "https://gitlab.com/shackra/goimapnotify"
  url "https://gitlab.com/shackra/goimapnotify/-/archive/2.5.8/goimapnotify-2.5.8.tar.bz2"
  sha256 "0d5764737ca6b76a3b4c0ddb25671de059abfe8b8e51686ffc3cf526bc605618"
  license "GPL-3.0-or-later"
  head "https://gitlab.com/shackra/goimapnotify.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "73493790b20bb0e2518b8f07b1ca057a9dc20a275d03cde3e63325c381c931e2"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.gittag=#{version}"), "./cmd/goimapnotify"
  end

  service do
    run [opt_bin/"goimapnotify"]
    keep_alive true
    log_path var/"log/goimapnotify.log"
    error_log_path var/"log/goimapnotify.log"
  end

  test do
    (testpath/"config.yml").write <<~YAML
      configurations:
        - username: test@example.com
    YAML

    output = shell_output("#{bin}/goimapnotify -conf #{testpath}/config.yml 2>&1", 1)
    assert_match "tag #{version}", output
    assert_match "empty or have invalid configuration format", output
  end
end
