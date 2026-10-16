class MlxC < Formula
  desc "C API for MLX, built in your CMake project against MLX's Windows build"
  homepage "https://ml-explore.github.io/mlx-c/build/html/index.html"
  url "https://github.com/ml-explore/mlx-c/archive/refs/tags/v0.7.0.tar.gz"
  sha256 "ee726bb38e191bb3c516a6bae47dc8abad9e5f273873385839019ce46bfceab5"
  license "MIT"

  # MLX runs on Windows with its CPU backend (the mlx formula installs ml-explore's official build),
  # and mlx-c supports Windows, but nobody publishes a prebuilt mlxc library for it and Windows has
  # no system C++ compiler for brew to build one. So this keg carries the C API headers plus the
  # mlx-c sources and a CMake package: find_package(MLXC) adds an `mlxc` target that compiles those
  # sources (C++20, MSVC) against MLX inside the consuming project, the same library mlx-c's own
  # CMakeLists builds with -DMLX_C_USE_SYSTEM_MLX=ON.
  #   cmake -S . -B build -DCMAKE_PREFIX_PATH="$(brew --prefix mlx-c);$(brew --prefix mlx)"
  #   target_link_libraries(app PRIVATE mlxc)

  depends_on "mlx"

  def install
    pkgshare.install "CMakeLists.txt", "mlx-c.pc.in", "mlx", "examples"
    (include/"mlx/c").mkpath
    (pkgshare/"mlx/c").glob("*.h").each { |header| cp header, include/"mlx/c" }

    src = opt_pkgshare.to_s.tr("\\", "/")
    mlx = (HOMEBREW_PREFIX/"opt/mlx").to_s.tr("\\", "/")
    (share/"cmake/MLXC/MLXCConfig.cmake").write <<~CMAKE
      # mlx-c for Windows: builds the mlxc library from the installed sources against MLX.
      include_guard(GLOBAL)
      include(CMakeFindDependencyMacro)
      find_dependency(MLX HINTS "#{mlx}")
      if(NOT TARGET mlxc)
        file(GLOB _mlxc_sources "#{src}/mlx/c/*.cpp")
        add_library(mlxc STATIC ${_mlxc_sources})
        target_compile_features(mlxc PUBLIC cxx_std_20)
        target_include_directories(mlxc PUBLIC "#{src}")
        target_link_libraries(mlxc PRIVATE mlx)
        unset(_mlxc_sources)
      endif()
      set(MLXC_LIBRARIES mlxc)
      set(MLXC_INCLUDE_DIRS "#{src}")
    CMAKE
    (share/"cmake/MLXC/MLXCConfigVersion.cmake").write <<~CMAKE
      set(PACKAGE_VERSION "#{version}")
      if(PACKAGE_FIND_VERSION VERSION_GREATER PACKAGE_VERSION)
        set(PACKAGE_VERSION_COMPATIBLE FALSE)
      else()
        set(PACKAGE_VERSION_COMPATIBLE TRUE)
        if(PACKAGE_FIND_VERSION STREQUAL PACKAGE_VERSION)
          set(PACKAGE_VERSION_EXACT TRUE)
        endif()
      endif()
    CMAKE
  end

  test do
    # No system C++ compiler on Windows to build homebrew-core's test program; check the C API
    # headers, the sources the CMake package compiles, and the package itself.
    mlx_h = (include/"mlx/c/mlx.h").read
    assert_match "mlx/c/ops.h", mlx_h
    assert_match "mlx_array_new_data", (include/"mlx/c/array.h").read
    assert_equal 26, (pkgshare/"mlx/c").glob("*.cpp").length
    assert_path_exists pkgshare/"examples/example.c"
    config = (share/"cmake/MLXC/MLXCConfig.cmake").read
    assert_match "find_dependency(MLX", config
    assert_match "add_library(mlxc STATIC", config
    assert_match version.to_s, (share/"cmake/MLXC/MLXCConfigVersion.cmake").read
  end
end
