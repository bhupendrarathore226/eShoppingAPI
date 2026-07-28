
import { RouterModule, Routes } from '@angular/router';
import { ProductControlComponent } from './product-control.component';
import { NgModule } from '@angular/core';

const routes: Routes = [ 
    { path:'', component: ProductControlComponent, data:{breadcrumb:'Product Control'} }    
]

@NgModule({
  declarations: [],
  imports: [
    RouterModule.forChild(routes)
  ],
  exports:[
    RouterModule
  ]
})
export class ProductRoutingModule { }