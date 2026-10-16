class Rabbitmqadmin < Formula
  desc "Command-line tool for RabbitMQ that uses the HTTP API"
  homepage "https://www.rabbitmq.com/docs/management-cli"
  url "https://github.com/rabbitmq/rabbitmqadmin-ng/archive/refs/tags/v2.35.0.tar.gz"
  sha256 "b52d5964ef296cf6a356522626a622225a825ab1248844172ace0058e211f988"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "37ec7ed1704fed955be4403f1deee49e309666d33cb807b1ff3b436985e07cd9"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args

    generate_completions_from_executable(bin/"rabbitmqadmin", "shell", "completions", "--shell")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rabbitmqadmin --help")
    assert_match "Configuration file '~/.rabbitmqadmin.conf' does not exist",
                 shell_output("#{bin}/rabbitmqadmin config_file show 2>&1", 65)
    assert_match "error sending request for url (http://localhost:15672/api/channels)",
                 shell_output("#{bin}/rabbitmqadmin list channels 2>&1", 65)
  end
end
