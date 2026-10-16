class Dumb < Formula
  desc "IT, XM, S3M and MOD player library"
  homepage "https://dumb.sourceforge.net/"
  url "https://github.com/kode54/dumb/archive/refs/tags/2.0.3.tar.gz"
  sha256 "99bfac926aeb8d476562303312d9f47fd05b43803050cd889b44da34a9b2a4f9"
  # Essentially Zlib but with modified and added clauses that aren't intended to restrict usage
  # https://src.fedoraproject.org/rpms/dumb/blob/rawhide/f/license-clarification.eml
  license :cannot_represent

  deprecate! date: "2025-07-03", because: :repo_archived
  disable! date: "2026-07-03", because: :repo_archived, replacement_formula: "libopenmpt"

  depends_on "cmake" => :build
  depends_on "argtable"
  depends_on "sdl2-compat"

  def install
    args = std_cmake_args + %w[
      -DBUILD_ALLEGRO4=OFF
      -DBUILD_EXAMPLES=ON
      -DCMAKE_POLICY_VERSION_MINIMUM=3.5
    ]

    # Build shared library
    system "cmake", "-S", ".", "-B", "build", *args, "-DBUILD_SHARED_LIBS=ON"
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"

    # Build static library
    system "cmake", "-S", ".", "-B", "build", *args, "-DBUILD_SHARED_LIBS=OFF"
    system "cmake", "--build", "build"
    lib.install "build/libdumb.a"
  end

  test do
    assert_match "missing option <file>", shell_output("#{bin}/dumbplay 2>&1", 1)
  end
end
