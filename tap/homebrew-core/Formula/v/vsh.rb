class Vsh < Formula
  desc "HashiCorp Vault interactive shell"
  homepage "https://github.com/fishi0x01/vsh"
  url "https://github.com/fishi0x01/vsh/archive/refs/tags/v1.0.0.tar.gz"
  sha256 "567ced47700cf36e0a542867e2b92f816757b62e149cc62002dc561ab2312cfd"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "440e17e1fd200a7a9303c77f8806e9e6e9a6a9a2944a0893bbb4069268aeca89"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.vshVersion=v#{version}")
  end

  test do
    version_output = shell_output("#{bin}/vsh --version")
    assert_match version.to_s, version_output
    error_output = shell_output("#{bin}/vsh -c ls 2>&1", 1)
    assert_match "Error initializing vault client | Is VAULT_ADDR properly set?", error_output
  end
end
