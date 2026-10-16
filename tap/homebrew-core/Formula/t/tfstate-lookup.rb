class TfstateLookup < Formula
  desc "Lookup resource attributes in tfstate"
  homepage "https://github.com/fujiwara/tfstate-lookup"
  url "https://github.com/fujiwara/tfstate-lookup/archive/refs/tags/v1.12.1.tar.gz"
  sha256 "d01dc5ca5193fd38696fcee0b8fd3f7211f9f12fdfefa46403a6159b9d677fa7"
  license "MPL-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7a144bf607f2d22fc01382b590abb9106c1229fd838ed9ddded92c17fd272f53"
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
