class Minisat < Formula
  desc "Minimalistic and high-performance SAT solver"
  homepage "https://github.com/stp/minisat"
  url "https://github.com/stp/minisat/archive/refs/tags/releases/2.2.1.tar.gz"
  sha256 "432985833596653fcd698ab439588471cc0f2437617d0df2bb191a0252ba423d"
  license "MIT"
  head "https://github.com/stp/minisat.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/minisat-2.2.1"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c3aced106ef74254996461502f9d771b72845e05ea76e4a5fcd744dee63e5902"
  end

  depends_on "cmake" => :build

  on_linux do
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build",
                    "-DCMAKE_INSTALL_RPATH=#{rpath}",
                    "-DCMAKE_POLICY_VERSION_MINIMUM=3.5",
                    "-DSTATIC_BINARIES=OFF",
                    *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cnf").write <<~EOS
      p cnf 5 3
      1 -5 4 0
      -1 5 3 4 0
      -3 -4 0
    EOS

    assert_match "SATISFIABLE", shell_output("#{bin}/minisat test.cnf 2>&1", 10)
  end
end
