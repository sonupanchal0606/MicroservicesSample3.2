# 🧩 Orchestration-Based Microservices Architecture with .NET and Ocelot

This project demonstrates a **microservices architecture** using **orchestration pattern** with direct **HTTP-based communication**. It includes:

- **Order Service**: Manages orders and coordinates with the Product service.
- **Product Service**: Manages product stock and pricing.
- **API Gateway**: Built using **Ocelot**, routes requests to respective services.

---

## 🏗️ Architecture Overview

![Architecture Diagram](./diagrams/orchestration-architecture.png)

### 🔁 Flow (Create Order)

1. Create Order Request
   - API Gateway receives the request and forwards to Order Service.
2. Order Service Orchestrates::
   - Calls ProductService via HTTP to get the product info (price, stock).
   - Checks availability and calculates total price.
   - Calls ProductService again to reduce the product quantity.
   - Stores order in OrderService database.
3. Get Orders with Product Info
   - OrderService stores only ProductId, Quantity, TotalPrice.
   - For /orders listing, it calls ProductService in parallel to enrich orders with product info (this is called API Composition).

---

## 🔧 Technologies Used

- **.NET 8 Web API**
- **Entity Framework Core (InMemory DB)**
- **Ocelot API Gateway**
- **IHttpClientFactory for service-to-service calls**
- **Minimal orchestration in controller logic**

---

## 🛠️ Project Structure

--- 

## ✅ Responsibilities
| Service            | Responsibilities                                                               |
| ------------------ | ------------------------------------------------------------------------------ |
| **ProductService** | CRUD for products, manage quantity, expose `/products` endpoints               |
| **OrderService**   | Orchestrates order creation/update/delete by interacting with `ProductService` |
| **API Gateway**    | Routes requests to correct services, aggregates Swagger                        |
| **PostgreSQL**     | Separate DB per service                                                        |

---

# 🏗️ Architecture Overview
```

                           ┌──────────────────────┐
                           │     Client (UI)      │
                           └──────────┬───────────┘
                                      │ HTTP Request
                                      ▼
                           ┌──────────────────────┐
                           │   API Gateway (Ocelot)│
                           │     Port: 9000        │
                           └──────────┬────────────┘
          ┌───────────────────────────┴──────────────────────────┐
          │                                                      │
          ▼                                                      ▼
┌──────────────────────┐                            ┌──────────────────────┐
│   Product Service     │                            │     Order Service     │
│  Port: 5001           │                            │  Port: 5002           │
│  PostgreSQL DB        │                            │  PostgreSQL DB        │
│  - Product CRUD       │                            │  - Order CRUD         │
│  - Reduce Quantity    │◄────────────┐              │  - Calls Product svc  │
└──────────────────────┘             │              └──────────────────────┘
                                     │
                                     │ REST Call
                                     ▼
                         ┌────────────────────────────┐
                         │ Orchestrator Logic in Order│
                         │   - CreateOrder:           │
                         │     1. Get Product Details │
                         │     2. Reduce Quantity     │
                         │     3. Save Order          │
                         │   - Update/Delete Order    │
                         │     ↳ Adjust Product Qty   │
                         └────────────────────────────┘

```

---

## 🔄 Benefits of Orchestration
- Centralized control over multi-service workflows.
- Easier error handling and rollback logic.
- OrderService ensures data consistency by updating both Order DB and Product DB directly.
