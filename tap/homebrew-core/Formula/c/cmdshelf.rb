class Cmdshelf < Formula
  desc "Better scripting life with cmdshelf"
  homepage "https://github.com/toshi0383/cmdshelf"
  url "https://github.com/toshi0383/cmdshelf/archive/refs/tags/2.0.2.tar.gz"
  sha256 "dea2ea567cfa67196664629ceda5bc775040b472c25e96944c19c74892d69539"
  license any_of: ["Apache-2.0", "MIT"]

  bottle do
    sha256 cellar: :any, x86_64_windows: "041c00ca565cf4b6e513e0adcb24523e6063266a3bc17dda8c31268658426830"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
    man.install Dir["docs/man/*"]
    bash_completion.install "cmdshelf-completion.bash" => "cmdshelf"
  end

  test do
    system bin/"cmdshelf", "remote", "add", "test", "git@github.com:toshi0383/scripts.git"
    list_output = shell_output("#{bin}/cmdshelf remote list").chomp
    assert_equal "test:git@github.com:toshi0383/scripts.git", list_output
  end
end
