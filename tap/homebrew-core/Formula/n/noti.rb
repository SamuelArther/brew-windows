class Noti < Formula
  desc "Trigger notifications when a process completes"
  homepage "https://codeberg.org/roble/noti"
  url "https://codeberg.org/roble/noti/releases/download/3.9.0/noti3.9.0.tar.gz"
  sha256 "02b18016f6a78a1adf3cdcc5b20d884437edd0cf21588415e022d637372619b8"
  license "MIT"
  head "https://codeberg.org/roble/noti.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "972f04f4c53d607908528af5bfe0378fba079abfd84c01f06515ddc2d271a769"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    ldflags = %W[-X codeberg.org/roble/noti/internal/command.Version=#{version}]
    system "go", "build", *std_go_args(ldflags:), "cmd/noti/main.go"
    man1.install "docs/man/dist/noti.1"
    man5.install "docs/man/dist/noti.yaml.5"

    generate_completions_from_executable(bin/"noti", shell_parameter_format: :cobra)
  end

  test do
    assert_match "noti version #{version}", shell_output("#{bin}/noti --version").chomp
    system bin/"noti", "-t", "Noti", "-m", "'Noti recipe installation test has finished.'"
  end
end
