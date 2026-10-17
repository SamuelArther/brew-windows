class SteamguardCli < Formula
  desc "CLI for steamguard"
  homepage "https://github.com/dyc3/steamguard-cli"
  url "https://github.com/dyc3/steamguard-cli/archive/refs/tags/v0.18.4.tar.gz"
  sha256 "a1beea2c0626ccda8a92abccd27fcb78b78a83d79f973095c972b913e088fdca"
  license "GPL-3.0-or-later"
  head "https://github.com/dyc3/steamguard-cli.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e1e54b6cc73d06bae4a521fbe3c66cf03b07f420d811d7f2b257c6db8fbf3742"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args

    generate_completions_from_executable(bin/"steamguard", "completion", shell_parameter_format: :arg)
  end

  test do
    require "pty"
    PTY.spawn(bin/"steamguard") do |stdout, stdin, _pid|
      stdin.puts "n\n"
      assert_match "Would you like to create a manifest in", stdout.read
    rescue Errno::EIO
      # GNU/Linux raises EIO when read is done on closed pty
    end
  end
end
