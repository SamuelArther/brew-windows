class Entt < Formula
  desc "Fast and reliable entity-component system for C++"
  homepage "https://github.com/skypjack/entt/wiki"
  url "https://github.com/skypjack/entt/archive/refs/tags/v4.0.0.tar.gz"
  sha256 "32a2ff2c72cb047dfd57306006ef238820b70da7c6ce4e7e8a507ac63365212e"
  license "MIT"
  head "https://github.com/skypjack/entt.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/entt-4.0.0"
    sha256 cellar: :any, x86_64_windows: "041d55a92e37520381507d17b34c7264e4b1eca5ed7c3e1af309d5d179caa571"
  end

  depends_on "cmake" => :build
  depends_on "pkgconf" => :test

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", "-DENTT_INSTALL=ON", "-DENTT_BUILD_DOCS=OFF", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <iostream>
      #include <entt/entt.hpp>

      struct Position { float x, y; };

      int main() {
        entt::registry registry;

        auto entity = registry.create();
        registry.emplace<Position>(entity, 1.0f, 2.0f);

        auto e2 = registry.create();
        registry.emplace<Position>(e2, -3.0f, 4.5f);

        auto view = registry.view<Position>();
        for (auto entity : view) {
          auto &pos = view.get<Position>(entity);
          std::cout << int(entity) << std::endl;
        }

        return 0;
      }
    CPP
    pkg_config_flags = shell_output("pkgconf --cflags --libs entt").chomp.split
    system ENV.cxx, "-std=c++20", *pkg_config_flags, "test.cpp", "-o", "test"
    system "./test"
  end
end
