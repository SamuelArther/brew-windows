class Fst < Formula
  desc "Represent large sets and maps compactly with finite state transducers"
  homepage "https://github.com/BurntSushi/fst"
  url "https://static.crates.io/crates/fst-bin/fst-bin-0.4.3.crate"
  sha256 "b3bc3f5b1becb288856c43673d440ab680d4c4d7221360247dd97cca00b5a20e"
  license any_of: ["Unlicense", "MIT"]
  head "https://github.com/BurntSushi/fst.git", branch: "master"

  livecheck do
    url :homepage
    regex(/^fst-bin[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "4ae857de0b6bb6df5ae487556cb1519ba438f3b8ba69cc7826e293f23f4bc009"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    cd(build.head? ? "fst-bin" : ".") do
      system "cargo", "generate-lockfile" if build.head?
      system "cargo", "fetch", *std_cargo_fetch_args
    end
  end

  def install
    cd(build.head? ? "fst-bin" : ".") do
      system "cargo", "install", *std_cargo_args
    end
  end

  test do
    (testpath/"map.csv").write <<~EOF
      four,4
      one,1
      twenty,20
      two,2
    EOF
    expected = <<~EOF
      twenty,20
      two,2
    EOF
    system bin/"fst", "map", "--sorted", testpath/"map.csv", testpath/"map.fst"
    assert_path_exists testpath/"map.fst"
    system bin/"fst", "verify", testpath/"map.fst"
    assert_equal expected, shell_output("#{bin}/fst grep -o #{testpath}/map.fst 'tw.*'")
  end
end
