class TfstateLookup < Formula
  desc "Lookup resource attributes in tfstate"
  homepage "https://github.com/fujiwara/tfstate-lookup"
  url "https://github.com/fujiwara/tfstate-lookup/archive/refs/tags/v1.12.2.tar.gz"
  sha256 "56cbf8902e8fd071030a8f0542e6672705d8296dd0e9478bc30a36a6e4a465f5"
  license "MPL-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "785c0b12fcd7c04a8f3f243ccca90c7f141d594471d804a8705a55d615f51698"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/tfstate-lookup"
  end

  test do
    (testpath/"terraform.tfstate").write <<~JSON
      {
        "version": 4,
        "terraform_version": "1.7.2",
        "resources": []
      }
    JSON

    output = shell_output("#{bin}/tfstate-lookup -dump")
    assert_match "{}", output
  end
end
